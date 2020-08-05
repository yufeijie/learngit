function plotParetoOnly(headline, subTitle1, subTitle2, subTitle3, subTitle4, xLabelName, yLabelName, zLabelName, dataPareto)
% plotPareto 画Pareto图
% headline 大标题
% subTitle1 2 3 4 子标题
% xLabelName yLabelName zLabelName 坐标轴名
% size 数据总数
% data  数据
% dataPareto Pareto最优数据
sz = 12;

subplot(3,2,1);
scatter3(dataPareto(:, 1), dataPareto(:, 2), dataPareto(:, 3), sz, getCmap(dataPareto(:, 5), 23, 33),  'filled');
grid on
title(subTitle1)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)

m = size(dataPareto, 1);

subplot(3,2,2);
scatter(dataPareto(:, 1), dataPareto(:, 2), sz, getCmap(dataPareto(:, 5), 23, 33), 'filled');
hold on
for i = 1 : m-1
    for j = i+1 : m
        if dataPareto(i, 2) < dataPareto(j, 2)
            dataPareto([i, j], :) = dataPareto([j, i], :);
        end
    end
end
n = 1;
pareto(1, 1) = dataPareto(1, 1);
pareto(1, 2) = dataPareto(1, 2);
for i = 2 : m
    while n > 0 && dataPareto(i, 1) < pareto(n, 1)
        n = n-1;
    end
    n = n+1;
    pareto(n, 1) = dataPareto(i, 1);
    pareto(n, 2) = dataPareto(i, 2);
end
%scatter(pareto(1:n, 1), pareto(1:n, 2), sz, 'r');
%hold on
%plot(pareto(1:n, 1), pareto(1:n, 2), 'r--');
%grid on
title(subTitle2)
xlabel(xLabelName)
ylabel(yLabelName)

subplot(3,2,3);
scatter(dataPareto(:, 2), dataPareto(:, 3), sz, getCmap(dataPareto(:, 5), 23, 33), 'filled');
hold on
for i = 1 : m-1
    for j = i+1 : m
        if dataPareto(i, 3) > dataPareto(j, 3)
            dataPareto([i, j], :) = dataPareto([j, i], :);
        end
    end
end
n = 1;
pareto(1, 1) = dataPareto(1, 2);
pareto(1, 2) = dataPareto(1, 3);
for i = 2 : m
    while n > 0 && dataPareto(i, 2) < pareto(n, 1)
        n = n-1;
    end
    n = n+1;
    pareto(n, 1) = dataPareto(i, 2);
    pareto(n, 2) = dataPareto(i, 3);
end
%scatter(pareto(1:n, 1), pareto(1:n, 2), sz, 'r');
%hold on
%plot(pareto(1:n, 1), pareto(1:n, 2), 'r--');
%grid on
title(subTitle3)
xlabel(yLabelName)
ylabel(zLabelName)

subplot(3,2,4);
scatter(dataPareto(:, 1), dataPareto(:, 3), sz, getCmap(dataPareto(:, 5), 23, 33), 'filled');
hold on
for i = 1 : m-1
    for j = i+1 : m
        if dataPareto(i, 3) > dataPareto(j, 3)
            dataPareto([i, j], :) = dataPareto([j, i], :);
        end
    end
end
n = 1;
pareto(1, 1) = dataPareto(1, 1);
pareto(1, 2) = dataPareto(1, 3);
for i = 2 : m
    while n > 0 && dataPareto(i, 1) < pareto(n, 1)
        n = n-1;
    end
    n = n+1;
    pareto(n, 1) = dataPareto(i, 1);
    pareto(n, 2) = dataPareto(i, 3);
end
%scatter(pareto(1:n, 1), pareto(1:n, 2), sz, 'r');
%hold on
%plot(pareto(1:n, 1), pareto(1:n, 2), 'r--');
%grid on
title(subTitle4)
xlabel(xLabelName)
ylabel(zLabelName)

%legend(ax4, [plot1, plot2, plot3 ,plot4 ,plot5], {'Feasible design based on SiC', 'Feasible design based on IGBT', '3D Pareto design', '3D Pareto design', '2D Pareto front'}, 'Location', 'east');
%legend('boxoff')

%suptitle(headline)
end

function cmap = getCmap(c, min, max)
shift = 0.2;

n = length(c);
cmap = zeros(n, 3);
for i = 1 : n
    normal = (c(i)-min)/(max-min);
    delta = (1-shift*2)*normal;
    cmap(i, 1) = shift+delta;
    cmap(i, 2) = shift+delta;
    cmap(i, 3) = 1;
end
end